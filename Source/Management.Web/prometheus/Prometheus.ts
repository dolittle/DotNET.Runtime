// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import parse from './parser';

export class Prometheus {
    static async getMetrics(): Promise<any> {
        const result = await fetch('/metrics');
        const metricsAsText = await result.text();
        const metrics = parse(metricsAsText);
        return metrics;
    }
}