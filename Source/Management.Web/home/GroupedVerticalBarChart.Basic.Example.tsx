// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from 'react';
import { DefaultPalette } from '@fluentui/react/lib/Styling';
import { GroupedVerticalBarChart, IGroupedVerticalBarChartProps } from '@fluentui/react-charting';
interface IGroupedBarChartState {
    width: number;
    height: number;
}

export class GroupedVerticalBarChartBasicExample extends React.Component<{}, IGroupedBarChartState> {
    constructor(props: IGroupedVerticalBarChartProps) {
        super(props);
        this.state = {
            width: 700,
            height: 400,
        };
    }

    public render(): JSX.Element {
        return <div>{this._basicExample()}</div>;
    }

    private _onWidthChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        this.setState({ width: parseInt(e.target.value, 10) });
    };
    private _onHeightChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        this.setState({ height: parseInt(e.target.value, 10) });
    };

    private _basicExample(): JSX.Element {
        const data = [
            {
                name: 'Metadata info multi lines text Completed',
                series: [
                    {
                        key: 'series1',
                        data: 33000,
                        color: DefaultPalette.blueLight,
                        legend: 'MetaData1',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '33%',
                    },
                    {
                        key: 'series2',
                        data: 44000,
                        color: DefaultPalette.blue,
                        legend: 'MetaData4',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '44%',
                    },
                ],
            },
            {
                name: 'Meta Data2',
                series: [
                    {
                        key: 'series1',
                        data: 33000,
                        color: DefaultPalette.blueLight,
                        legend: 'MetaData1',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '33%',
                    },
                    {
                        key: 'series2',
                        data: 3000,
                        color: DefaultPalette.blue,
                        legend: 'MetaData4',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '3%',
                    },
                ],
            },

            {
                name: 'Single line text ',
                series: [
                    {
                        key: 'series1',
                        data: 14000,
                        color: DefaultPalette.blueLight,
                        legend: 'MetaData1',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '14%',
                    },
                    {
                        key: 'series2',
                        data: 50000,
                        color: DefaultPalette.blue,
                        legend: 'MetaData4',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '50%',
                    },
                ],
            },
            {
                name: 'Hello World!!!',
                series: [
                    {
                        key: 'series1',
                        data: 33000,
                        color: DefaultPalette.blueLight,
                        legend: 'MetaData1',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '33%',
                    },
                    {
                        key: 'series2',
                        data: 3000,
                        color: DefaultPalette.blue,
                        legend: 'MetaData4',
                        xAxisCalloutData: '2020/04/30',
                        yAxisCalloutData: '3%',
                    },
                ],
            },
        ];

        const rootStyle = { width: `${this.state.width}px`, height: `${this.state.height}px` };
        return (
            <>
                <div style={rootStyle}>
                    <GroupedVerticalBarChart
                        data={data}
                        height={this.state.height}
                        width={this.state.width}
                        showYAxisGridLines
                        wrapXAxisLables
                    />
                </div>
            </>
        );
    }
}
