// `make web` in the devcontainer (ng serve --configuration devcontainer). Same as environment.ts
// except:
// - the API: the devcontainer runs Neptune.API over plain HTTP on API_PORT (8250) instead of the
//   Visual Studio docker-compose stack on 8212.
// - redirectUri is unset, so Auth0 returns to the page's own origin + /callback (app.config.ts).
//   The devcontainer serves on neptune.localhost.sitkatech.com:WEB_PORT (8252); that origin must be
//   in the Auth0 app's Allowed Callback / Logout URLs and Web Origins to sign in.
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
        redirectUri: undefined as string | undefined,
        audience: "OCSTApi",
    },
};
