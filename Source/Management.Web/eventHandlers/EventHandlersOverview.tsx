// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useEffect, useState } from 'react';
import { useLazyQuery, useQuery } from '@apollo/client';
import {
    CommandBar,
    Dropdown,
    getTheme,
    IColumn,
    ICommandBarItemProps,
    IDropdownOption,
    Icon,
    ShimmeredDetailsList,
    Stack,
    Text
} from '@fluentui/react';
import { eventHandlersQuery } from './eventHandlersQuery';
import { tenantsQuery } from './tenantsQuery';

const columns: IColumn[] = [
    {
        key: 'id',
        fieldName: 'id',
        name: 'Id',
        minWidth: 250
    },
    {
        key: 'scope',
        fieldName: 'scope',
        name: 'Scope',
        minWidth: 250
    },
    {
        key: 'lastCommittedEventSequenceNumber',
        fieldName: 'lastCommittedEventSequenceNumber',
        name: 'Last Committed Sequence Number',
        minWidth: 100
    },
    {
        key: 'filterPosition',
        fieldName: 'filterPosition',
        name: 'Filter Position',
        minWidth: 100
    },
    {
        key: 'eventProcessorPosition',
        fieldName: 'eventProcessorPosition',
        name: 'Event Processor Position',
        minWidth: 100
    }
];


export const EventHandlersOverview = () => {
    const [tenantId, setTenantId] = useState<string>();
    const tenantsQueryResult = useQuery(tenantsQuery);
    const [getEventHandlers, eventHandlersQueryResult] = useLazyQuery(eventHandlersQuery);
    const [eventHandlers, setEventHandlers] = useState([]);

    const tenantsOptions = tenantsQueryResult.data?.tenancy?.all.map(_ => {
        return {
            key: _,
            text: _
        } as IDropdownOption;
    });

    const filterEventHandlersForTenant = (tenantId: string) => {
        setEventHandlers(eventHandlersQueryResult.data?.eventHandlers?.all.map(_ => {
            const status = _.statusPerTenant.find(_ => _.tenantId == tenantId);
            return {
                id: _.id,
                scope: _.scope,
                lastCommittedEventSequenceNumber: status.lastCommittedEventSequenceNumber,
                filterPosition: status.filterPosition,
                eventProcessorPosition: status.eventProcessorPosition
            };
        }) || []);
    };

    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'tenant',
            text: 'Tenant',
            onRender: () => {
                const theme = getTheme();

                return (
                    <Stack horizontal style={{ alignItems: 'center' }} tokens={{ childrenGap: 8 }} >
                        <Icon iconName="Database" style={{ color: theme.palette.themePrimary, fontSize: theme.fonts.mediumPlus.fontSize }} />
                        <Text variant="medium">Tenant</Text>
                        <Dropdown
                            style={{ width: 200 }}
                            placeholder="Select tenant"
                            defaultSelectedKey="local"
                            selectedKey={tenantId}
                            onChange={(e, o) => {
                                setTenantId(o?.key as string);
                                filterEventHandlersForTenant(o?.key as string);
                            }}
                            options={tenantsOptions} />
                    </Stack>
                );
            }
        },
        {
            key: 'refresh',
            text: 'Refresh',
            iconProps: { iconName: 'Refresh' },
            onClick: () => getEventHandlers()
        }
    ];

    useEffect(() => {
        getEventHandlers();
    }, []);

    return (
        <>
            <Stack>
                <CommandBar
                    styles={{
                        root: {
                            alignItems: 'center',
                        },
                    }}
                    items={commandBarItems} />

                <ShimmeredDetailsList columns={columns} items={eventHandlers} enableShimmer={eventHandlersQueryResult.loading} />
            </Stack>
        </>
    );
};
