window.passkeyInterop = {
    isSupported: function () {
        return !!(window.PublicKeyCredential);
    },

    register: async function (optionsJson) {
        const options = JSON.parse(optionsJson);
        options.challenge = base64urlToBuffer(options.challenge);
        options.user.id = base64urlToBuffer(options.user.id);

        if (options.excludeCredentials) {
            options.excludeCredentials = options.excludeCredentials.map(c => ({
                ...c,
                id: base64urlToBuffer(c.id)
            }));
        }

        const credential = await navigator.credentials.create({ publicKey: options });

        return JSON.stringify({
            id: credential.id,
            rawId: bufferToBase64url(credential.rawId),
            type: credential.type,
            response: {
                attestationObject: bufferToBase64url(credential.response.attestationObject),
                clientDataJSON: bufferToBase64url(credential.response.clientDataJSON)
            }
        });
    },

    authenticate: async function (optionsJson) {
        const options = JSON.parse(optionsJson);
        options.challenge = base64urlToBuffer(options.challenge);

        if (options.allowCredentials) {
            options.allowCredentials = options.allowCredentials.map(c => ({
                ...c,
                id: base64urlToBuffer(c.id)
            }));
        }

        const assertion = await navigator.credentials.get({ publicKey: options });

        return JSON.stringify({
            id: assertion.id,
            rawId: bufferToBase64url(assertion.rawId),
            type: assertion.type,
            response: {
                authenticatorData: bufferToBase64url(assertion.response.authenticatorData),
                clientDataJSON: bufferToBase64url(assertion.response.clientDataJSON),
                signature: bufferToBase64url(assertion.response.signature),
                userHandle: assertion.response.userHandle
                    ? bufferToBase64url(assertion.response.userHandle)
                    : null
            }
        });
    }
};

function base64urlToBuffer(value) {
    if (value instanceof ArrayBuffer || ArrayBuffer.isView(value)) return value;
    const padding = '='.repeat((4 - value.length % 4) % 4);
    const base64 = (value + padding).replace(/-/g, '+').replace(/_/g, '/');
    const binary = atob(base64);
    const buffer = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) buffer[i] = binary.charCodeAt(i);
    return buffer.buffer;
}

function bufferToBase64url(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) binary += String.fromCharCode(bytes[i]);
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}
