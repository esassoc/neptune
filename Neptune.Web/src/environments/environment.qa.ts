export const environment = {
    production: false,
    staging: true,
    dev: false,
    mainAppApiUrl: "https://qa-internalapi.ocstormwatertools.org",
    externalApiScalarUrl: "https://qa-api.ocstormwatertools.org/docs",
    geoserverMapServiceUrl: "https://qa-mapserver.ocstormwatertools.org/geoserver/OCStormwater",
    datadogClientToken: "pub6bc5bcb39be6b4c926271a35cb8cb46a",
    auth0: {
        domain: "ocstormwatertools.us.auth0.com",
        clientId: "ifBEaIsDKHXBQoIyDVl1CB21avZh1xEx",
        redirectUri: "https://qa-web.ocstormwatertools.org/callback",
        audience: "OCSTApi",
    },
};
