// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createTheme, loadTheme } from '@fluentui/react/lib/Styling';
import { initializeIcons } from '@fluentui/font-icons-mdl2';

import './theme.scss';

initializeIcons();

const myTheme = createTheme({
    palette: {
        themePrimary: '#ffcf00',
        themeLighterAlt: '#fffdf5',
        themeLighter: '#fff8d6',
        themeLight: '#fff1b3',
        themeTertiary: '#ffe366',
        themeSecondary: '#ffd61f',
        themeDarkAlt: '#e6bb00',
        themeDark: '#c29e00',
        themeDarker: '#8f7500',
        neutralLighterAlt: '#292d32',
        neutralLighter: '#292c31',
        neutralLight: '#272a2f',
        neutralQuaternaryAlt: '#24282c',
        neutralQuaternary: '#23262a',
        neutralTertiaryAlt: '#212428',
        neutralTertiary: '#c8c8c8',
        neutralSecondary: '#d0d0d0',
        neutralPrimaryAlt: '#dadada',
        neutralPrimary: '#ffffff',
        neutralDark: '#f4f4f4',
        black: '#f8f8f8',
        white: '#2b2f34',
    }
});

loadTheme(myTheme);
