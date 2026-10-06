// `make web` in the devcontainer (ng serve --configuration devcontainer). Same as environment.ts
// except the API: the devcontainer runs Neptune.API over plain HTTP on API_PORT (8250) instead of
// the Visual Studio docker-compose stack on 8212. The SPA itself is still served on
// neptune.localhost.sitkatech.com:8213 — the only local origin registered in Auth0.
export const environment = {
    production: false,
    staging: false,
    dev: true,
    mainAppApiUrl: "http://localhost:8250",
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
