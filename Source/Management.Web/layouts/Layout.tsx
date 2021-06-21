// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { AppHeader } from './AppHeader';
import { TopLevelMenu } from './TopLevelMenu';

export const Layout = () => {
    return (
        <>
            <AppHeader />
            <TopLevelMenu />
        </>
    );
};