FROM lonwern/aspnetcore-libgdiplus:3.1

WORKDIR /home/Apps/Face3.0/
EXPOSE 80
COPY . .
COPY ./libarcsoft_face.so /usr/lib/
COPY ./libarcsoft_face_engine.so /usr/lib/
ENTRYPOINT ["dotnet", "MicroService_Face_3_0.dll"]
