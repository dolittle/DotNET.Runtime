// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandBar, DetailsList, IColumn, ICommandBarItemProps, Stack } from '@fluentui/react';
import React, { useEffect, useState } from 'react';
import { Prometheus } from '../prometheus/Prometheus';

const columns: IColumn[] = [
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    },
    {
        key: 'description',
        name: 'Description',
        fieldName: 'help',
        minWidth: 300
    },
    {
        key: 'metrics',
        name: 'Metrics',
        fieldName: 'metrics',
        minWidth: 300,
        onRender: (data) => {
            const values = data.metrics.map(_ => _.value);
            const valueString = values.join(', ');
            return (
                <div>{valueString}</div>
            );
        }
    }
];



export const Metrics = () => {
    const [metrics, setMetrics] = useState<any>({});

    const loadData = () => {
        Prometheus.getMetrics().then(_ => {
            setMetrics(_);
        });
    };

    const commandBarItems: ICommandBarItemProps[] = [
        { key: 'refresh', text: 'Refresh', iconProps: { iconName: 'Refresh' }, onClick:loadData }
    ];

    useEffect(() => {
        loadData();
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

                <DetailsList columns={columns} items={metrics} />
            </Stack>
        </>
    );
};