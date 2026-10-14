# Receta para armar la "caja" (contenedor) que corre tu servidor en internet.
# Primera etapa: compilar el proyecto con el SDK de .NET.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilar
WORKDIR /src
COPY Crave.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish Crave.csproj -c Release -o /app

# Segunda etapa: una imagen liviana que solo ejecuta el resultado.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilar /app .

# Render espera que el servidor escuche en el puerto 10000.
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "Crave.dll"]
