# SlowStartService
Example of how to signal Windows SCM as early as possible

# The issue
What happens during Windows boot is many services are fighting for resources to start up and aspnetcore projects timeout within the 30 seconds.

It happens because the regular WindowsServiceLifetime doesn't get called until `app.Run()`. 
