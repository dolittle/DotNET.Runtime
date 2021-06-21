// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from 'react';

import './AppHeader.scss';

const logo = require('./logo.svg');

export const AppHeader = () => {
    return (
        <header className="app-header">
            <a href="/" className="site-logo">
                <img src={logo} alt="" />
            </a>
        </header>
    );
};
