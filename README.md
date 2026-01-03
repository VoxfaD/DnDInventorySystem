For visual studio code you will need c#, c# dev kit and .NET install tool installed on, for visual studio code you will have to open up the
all of the file and in visual studio code terminal go to where this: ../DnDInventorySystem/DnDInventorySystem is located then run in order these commands:

(make sure you have created a migrations folder here ../DnDInventorySystem/DnDInventorySystem/Migrations)
dotnet ef migrations add InitialCreate
dotnet ef database update 

for removal run these:
dotnet ef database drop
dotnet ef migrations remove

and for building and running the project run these:
dotnet run
dotnet build

for visual studio open up the DnDInventorySystem.slnx and in developer powershell (make sure it is in file ../DnDInventorySystem/DnDInventorySystem) run the same commands, but
for running the project you can click on where there is a green triangle and it says https
