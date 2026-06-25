export const environment = {
    production: false,
    staging: false,
    dev: true,
    mainAppApiUrl: "https://host.docker.internal:8212",
    externalApiScalarUrl: "https://host.docker.internal:8241/docs",
    geoserverMapServiceUrl: "http://localhost:8780/geoserver/OCStormwater",
    datadogClientToken: "pub6bc5bcb39be6b4c926271a35cb8cb46a",
    auth0: {
        domain: "ocstormwatertools.us.auth0.com",
        clientId: "ifBEaIsDKHXBQoIyDVl1CB21avZh1xEx",
        redirectUri: "https://neptune.localhost.sitkatech.com:8213/callback",
        audience: "OCSTApi",
    },
};
