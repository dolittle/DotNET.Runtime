// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const webpack = require('./WebPack');
module.exports = (env, argv) => {
    return webpack(env, argv, '/', config => {
        config.devServer.proxy = {
            '/graphql': 'http://localhost:8001',
            '/graphql/ui': 'http://localhost:8001',
            '/api': 'http://localhost:8001'
        };
    }, 9000, 'Dolittle Runtime Management UI');
};
