# to build
# docker build  --tag atlas_demo . --build-arg HOST=host PASSWORD=password USER=user

# to run locally
# docker run -i -t -p 1234:1234 -p 8983:8983 -e PORT=1234  -u 0 atlas_demo:latest

# to get in shell
# docker run --entrypoint sh -i -t -u 0 atlas_demo:latest

# to access webapp
# http://localhost:1234

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /app
COPY web.sln .
COPY ["./web/web.csproj", "./web/"]
RUN dotnet restore

COPY ["./web/.", "./web/"]
WORKDIR "/app/web/"
# add analytics
RUN  sed -i -e 's/<\/body>/<script async defer data-website-id="833156f8-3343-4da3-b7d5-45b5fa4f224d" src="https:\/\/analytics.atlas.bi\/umami.js"><\/script><\/body>/g' Pages/Shared/_Layout.cshtml
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine
WORKDIR /app
COPY --from=build ["/app/web/out", "./"]

ARG USER
ARG PASSWORD
ARG HOST

# create config
RUN echo "{\"solr\": {\"atlas_address\": \"https://atlas-dotnet-search.herokuapp.com/solr/atlas\"},\"ConnectionStrings\": {\"AtlasDatabase\": \"Server=$HOST;Database=atlas;User Id=$USER; Password=$PASSWORD; MultipleActiveResultSets=true\"}}" > appsettings.cust.json

CMD ASPNETCORE_URLS=http://*:$PORT dotnet "Atlas_Web.dll"
