FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

RUN apt-get update  
RUN apt-get install -y git
RUN apt-get install -y net-tools
COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh

ENV DATA_PATH /app/Common 
ENV MAP_DATA_PATH /app/Common/MapData 

EXPOSE 7777

ENTRYPOINT ["/entrypoint.sh"]

WORKDIR /app/Server
#CMD ["dotnet", "watch", "run"]
CMD ["sh", "-c", "dotnet restore && dotnet watch run"]