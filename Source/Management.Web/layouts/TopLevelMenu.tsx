// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { Pivot, PivotItem } from '@fluentui/react';
import { useHistory, useLocation } from 'react-router-dom';

import './TopLevelMenu.scss';

export const TopLevelMenu = () => {
    const location = useLocation();
    const history = useHistory();

    function linkClicked(item?: PivotItem, ev?: React.MouseEvent<HTMLElement>) {
        history.push(item?.props.itemKey!);
    }

    return (
        <div className="top-level-menu">
            <Pivot onLinkClick={linkClicked} selectedKey={location.pathname}>
                <PivotItem headerText="Home" itemKey="/"></PivotItem>
                <PivotItem headerText="Metrics" itemKey="/metrics-list"></PivotItem>
                <PivotItem headerText="EventHandlers" itemKey="/event-handlers/"></PivotItem>
            </Pivot>
        </div>
    );
};
