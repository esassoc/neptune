export const environment = {
    production: true,
    staging: false,
    dev: false,
    mainAppApiUrl: "https://internalapi.ocstormwatertools.org",
    externalApiScalarUrl: "https://api.ocstormwatertools.org/docs",
    geoserverMapServiceUrl: "https://mapserver.ocstormwatertools.org/geoserver/OCStormwater",
    datadogClientToken: "pub6bc5bcb39be6b4c926271a35cb8cb46a",
    auth0: {
        domain: "ocstormwatertools.us.auth0.com",
        clientId: "ifBEaIsDKHXBQoIyDVl1CB21avZh1xEx",
        redirectUri: "https://www.ocstormwatertools.org/callback",
        audience: "OCSTApi",
    },
};
