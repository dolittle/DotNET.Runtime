// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dolittle.Runtime.Collections;
using Dolittle.Runtime.DependencyInversion;
using Dolittle.Runtime.Events.Store.Streams.Filters;
using Dolittle.Runtime.Lifecycle;
using Microsoft.Extensions.Logging;
using Dolittle.Runtime.Types;
using Dolittle.Runtime.Rudimentary;
using Dolittle.Runtime.Execution;
using Dolittle.Runtime.Events.Processing.Streams;
using Dolittle.Runtime.Events.Store.Streams;

namespace Dolittle.Runtime.Events.Processing.Filters
{
    /// <summary>
    /// Represents an implementation of <see cref="IFilterValidators" />.
    /// </summary>
    [Singleton]
    public class FilterValidators : IFilterValidators
    {
        readonly ITypeFinder _typeFinder;
        readonly IContainer _container;
        readonly FactoryFor<IStreamProcessorStateRepository> _getStreamProcessorStates;
        readonly FactoryFor<IFilterDefinitions> _getFilterDefinitions;
        readonly IExecutionContextManager _executionContextManager;
        readonly ICompareFilterDefinitions _definitionComparer;
        readonly ILogger _logger;
        readonly IDictionary<Type, Type> _filterDefinitionToValidatorMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterValidators"/> class.
        /// </summary>
        /// <param name="typeFinder">The <see cref="ITypeFinder" />.</param>
        /// <param name="container">The <see cref="IContainer" />.</param>
        /// <param name="getFilterDefinitions">The <see cref="FactoryFor{T}" /> <see cref="IFilterDefinitions" />.</param>
        /// <param name="executionContextManager">The <see cref="IExecutionContextManager" />.</param>
        /// <param name="definitionComparer">The <see cref="ICompareFilterDefinitions" />.</param>
        /// <param name="logger">The <see cref="ILogger" />.</param>
        public FilterValidators(
            ITypeFinder typeFinder,
            IContainer container,
            FactoryFor<IStreamProcessorStateRepository> getStreamProcessorStates,
            FactoryFor<IFilterDefinitions> getFilterDefinitions,
            IExecutionContextManager executionContextManager,
            ICompareFilterDefinitions definitionComparer,
            ILogger logger)
        {
            _typeFinder = typeFinder;
            _container = container;
            _getStreamProcessorStates = getStreamProcessorStates;
            _getFilterDefinitions = getFilterDefinitions;
            _executionContextManager = executionContextManager;
            _definitionComparer = definitionComparer;
            _logger = logger;
            PopulateFilterValidatorMap();
        }

        /// <inheritdoc/>
        public async Task<FilterValidationResult> Validate<TDefinition>(IFilterProcessor<TDefinition> filter, CancellationToken cancellationToken)
            where TDefinition : IFilterDefinition
        {
            _logger.LogTrace("Starting Validate for source {Source}, target {Target}, isPartitioned: {Partitioned}, isPublic: {Public}", filter.Definition.SourceStream, filter.Definition.TargetStream, filter.Definition.Partitioned, filter.Definition.Public);
            var tryGetProcessorState = await _getStreamProcessorStates()
                .TryGetFor(new StreamProcessorId(filter.Scope, filter.Definition.TargetStream.Value, filter.Definition.SourceStream), cancellationToken)
                .ConfigureAwait(false);

            if (!StreamProcessorHasProcessedEvents(tryGetProcessorState, out var validationResult, out var lastUnprocessedEvent))
            {
                return validationResult;
            }

            _logger.TryGetFilterDefinition(filter.Identifier, _executionContextManager.Current.Tenant);
            var tryGetFilterDefinition = await _getFilterDefinitions().TryGetFromStream(filter.Scope, filter.Definition.TargetStream, cancellationToken).ConfigureAwait(false);

            if (!FilterDefinitionHasBeenPersisted(tryGetFilterDefinition, out var persistedDefinition, out validationResult))
            {
                _logger.NoPersistedFilterDefinition(filter.Identifier, _executionContextManager.Current.Tenant);
                return validationResult;
            }

            var definitionResult = _definitionComparer.DefinitionsAreEqual(persistedDefinition, filter.Definition);
            if (!definitionResult.Succeeded)
            {
                return definitionResult;
            }

            if (FilterDefinitionTypeHasChanged(persistedDefinition, filter.Definition))
            {
                return new FilterValidationResult("Filter definition type has changed");
            }

            _logger.FindingFilterValidator(filter.Identifier);
            if (!TryGetValidatorFor<TDefinition>(out var validator))
            {
                return new FilterValidationResult($"No available filter validator for type {filter.Definition.GetType()}");
            }

            _logger.ValidatingFilter(filter.Identifier);
            return await validator.Validate((TDefinition)persistedDefinition, filter, lastUnprocessedEvent, cancellationToken).ConfigureAwait(false);
        }

        bool StreamProcessorHasProcessedEvents(Try<IStreamProcessorState> tryGetState, out FilterValidationResult validationResult, out StreamPosition lastUnprocessedEvent)
        {
            if (tryGetState.HasException)
            {
                validationResult = new FilterValidationResult(tryGetState.Exception.Message);
                lastUnprocessedEvent = default;
                return false;
            }

            if (!tryGetState.Success)
            {
                validationResult = new FilterValidationResult();
                lastUnprocessedEvent = default;
                return false;
            }

            lastUnprocessedEvent = tryGetState.Result.Position;

            if (lastUnprocessedEvent == StreamPosition.Start)
            {
                validationResult = new FilterValidationResult();
                return false;
            }

            validationResult = default;
            return true;
        }

        bool FilterDefinitionHasBeenPersisted(Try<IFilterDefinition> tryGetFilterDefinition, out IFilterDefinition persistedDefinition, out FilterValidationResult validationResult)
        {
            if (tryGetFilterDefinition.HasException)
            {
                persistedDefinition = default;
                validationResult = new FilterValidationResult(tryGetFilterDefinition.Exception.Message);
                return false;
            }

            if (!tryGetFilterDefinition.Success)
            {
                persistedDefinition = default;
                validationResult = new FilterValidationResult();
                return false;
            }

            persistedDefinition = tryGetFilterDefinition.Result;
            validationResult = default;
            return true;
        }

        bool FilterDefinitionTypeHasChanged<TDefinition>(IFilterDefinition persitedDefiniton, TDefinition registeredDefinition)
            => persitedDefiniton.GetType() != registeredDefinition.GetType();

        void PopulateFilterValidatorMap()
        {
            _typeFinder.FindMultiple<IFilterDefinition>().ForEach(filterDefinitionType =>
            {
                if (TryGetValidatorTypeFor(filterDefinitionType, out var validatorType))
                {
                    _logger.FoundValidatorForFilter(filterDefinitionType, validatorType);
                    _filterDefinitionToValidatorMap.TryAdd(filterDefinitionType, validatorType);
                }
            });
        }

        bool TryGetValidatorTypeFor(Type filterDefinitionType, out Type validatorType)
        {
            var implementations = _typeFinder.FindMultiple(typeof(ICanValidateFilterFor<>).MakeGenericType(filterDefinitionType));
            if (implementations.Any())
            {
                if (implementations.Count() > 1)
                {
                    _logger.MultipleValidatorsForFilter(
                        filterDefinitionType,
                        implementations);
                }

                validatorType = implementations.First();
                return true;
            }

            validatorType = null;
            return false;
        }

        bool TryGetValidatorFor<TDefinition>(out ICanValidateFilterFor<TDefinition> validator)
            where TDefinition : IFilterDefinition
        {
            if (_filterDefinitionToValidatorMap.TryGetValue(typeof(TDefinition), out var validatorType))
            {
                validator = _container.Get(validatorType) as ICanValidateFilterFor<TDefinition>;
                return true;
            }

            validator = null;
            return false;
        }
    }
}
