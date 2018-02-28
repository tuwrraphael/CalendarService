# CalendarService
A service to integrate calendar based experiences into applications.
It solves the problem of merging calendars of different providers for a user and provides a single calendar endpoint for an application.

## Account-Linking
Users link their calendar providing accounts (Microsoft, Google) to your applications
identity (provided by any openid connect server, for example IdentityServer). This is done via the respective Authentication Flow of the third
party calendar provider (eg. OAuth 2.0 authorization code flow at Microsoft).

## Authentication
The CalendarService uses Authentication via scopes which are provided in bearer tokens created by an external openid connect server.

Requests authenticated with a bearer token that contains the *calendar.user* scope can link/unlink accounts, configure
which calendars(feeds) of an account to merge, and view the own merged calendar endpoint for the subject which the bearer token belongs to.

Requests authenticated with a bearer token that contains the *calendar.service* scope can access the merged calendar
endpoint for any identity, it just passes the requested subject by parameter.

This allows two scenarios: Users obtain a bearer token for example by OAuth implicit flow from your identity provider. Using that they can
configure linked calendars. Your services which want to provide rich personalized experiences for your users then obtain a bearer token for
example via client credentials flow, and can access all your users calendars through a single endpoint.

On this way the CalendarService abstracts the different calendar APIs as well as account linking and token management in form of
a microservice which can be integrated into your service architecture.

## Configuration
Obtain as clientId and secret by registering a converged application with 
the offline_access and Calendars.Read scopes at https://apps.dev.microsoft.com/.
The redirect uri should match where you host the service followed by /api/configuration/ms-connect.

Your openid connect server should be able to provide the calendar.user and calendar.service scopes (be sure not to provide the
calendar.service scope to every user but only to your service clients). Change the scopes names in the Startup.cs.

The app settings should contain these parameters:
~~~
        "MSClientId": "Microsoft Graph clientId",
        "MSSecret": "Microsoft Graph secret",
        "MSRedirectUri": "http://localhost:12345/api/configuration/ms-connect", 
        "ServiceIdentityUrl" : "Url to your openid connect server"
~~~

## Installation
Before publishing the CalendarSerivice you should create the CalendarService\CalendarService\wwwroot\App_Data folder and run
~~~
Update-Database
~~~
in the package manager console. This will create the initial Sqlite database.
