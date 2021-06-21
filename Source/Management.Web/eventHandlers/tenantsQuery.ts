// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { gql } from '@apollo/client';

export const tenantsQuery = gql`
    query {
        tenancy {
            all
        }
    }
`;
