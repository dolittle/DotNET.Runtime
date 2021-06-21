// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

class InvalidLineError extends Error {
    constructor(message) {
        super('Encountered invalid line: ' + message);
    }
}

Object.defineProperty(InvalidLineError.prototype, 'name', {
    value: InvalidLineError.name
});

export default InvalidLineError;