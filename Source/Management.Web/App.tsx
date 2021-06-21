// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { Layout } from './layouts/Layout';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import './App.scss';
import { EventHandlersOverview } from './eventHandlers/EventHandlersOverview';
import { Dashboard } from './home/Dashboard';
import { Metrics } from './metrics/Metrics';


export const App = () => {
    return (
        <>
            <Router>
                <Layout />
                <div className="content">
                    
                <Route exact path="/">
                        <Dashboard/>
                    </Route>
                    <Route exact path="/metrics-list">
                        <Metrics/>
                    </Route>
                    <Route path="/event-handlers">
                        <EventHandlersOverview/>
                    </Route>
                </div>
            </Router>
        </>
    )
};