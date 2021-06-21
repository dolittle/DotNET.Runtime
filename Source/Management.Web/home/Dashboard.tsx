// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';

import { Stack } from '@fluentui/react';
import { HeatMapChartBasicExample } from './HeatMapChartBasic.Example';
import { LineChartBasicExample } from './LineChart.Basic.Example';
import { PieChartBasicExample } from './PieChart.Basic.Example';
import { GroupedVerticalBarChartBasicExample } from './GroupedVerticalBarChart.Basic.Example';


export const Dashboard = () => {
    return (
        <>
            <div style={{ margin: '1rem' }}>
                <Stack horizontal>
                    <HeatMapChartBasicExample />
                    <LineChartBasicExample />
                </Stack>
                <Stack horizontal>
                    <GroupedVerticalBarChartBasicExample />
                    <PieChartBasicExample />
                </Stack>
            </div>
        </>
    );
}