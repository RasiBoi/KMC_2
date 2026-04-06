# KMC Event Management - Run Guide

## 1. Prerequisites (one time)

Install these on Windows:
- Visual Studio 2022 (ASP.NET and web development + .NET desktop development workloads)
- Visual Studio Code
- SQL Server (Express/Developer is fine)
- SQL Server Management Studio (SSMS)
- .NET Framework 4.7.2 Developer/Targeting Pack
- NuGet CLI and MSBuild (included with Visual Studio Build Tools/Visual Studio)
- IIS Express (installed with Visual Studio)

## 2. Database setup (one time)

1. Open SSMS and connect to your SQL Server instance.
2. Open `db.sql` from the project root.
3. Execute the full script.
4. Confirm database `KMC_CityEventsDB` is created.

> Note: the API currently uses this connection string in code:
> `Server=.;Database=KMC_CityEventsDB;Trusted_Connection=True;`
> If your SQL instance is different, update it in `KMCEvent.Api/Data/DatabaseManager.cs`.

---

## 3. Run using Visual Studio 2022

### Backend first (ASMX)

1. Open `KMCEventManagement.sln` in Visual Studio.
2. Restore NuGet packages (right-click solution -> Restore NuGet Packages).
3. Right-click project `KMCEvent.Api` -> Set as Startup Project.
4. In `KMCEvent.Api` web properties, ensure project URL uses port `44356` (or keep current URL and then update client endpoint to match).
5. Start backend (`F5` or `Ctrl+F5`).
6. Verify browser opens:
   - `http://localhost:44356/API.asmx`

### Frontend second (WinForms)

1. Keep backend running.
2. Right-click project `KMCEvent.Client` -> Set as Startup Project.
3. Confirm client endpoint in `KMCEvent.Client/App.config` points to your backend URL:
   - `http://localhost:44356/API.asmx`
4. Start frontend (`F5` or `Ctrl+F5`).
5. Login with seeded users from `db.sql` (password for all seeded users is `1234`):
   - `organizer1`
   - `participant1`
   - `public1`

---

## 4. Run using Visual Studio Code

Open VS Code in the repository root (`KMCEventManagement`).

### Backend first (ASMX)

1. Open terminal in VS Code.
2. Restore packages:

```powershell
nuget restore .\KMCEventManagement.sln
```

3. Build backend project:

```powershell
msbuild .\KMCEvent.Api\KMCEvent.Api.csproj /t:Build /p:Configuration=Debug
```

4. Run backend with IIS Express (keep this terminal open):

```powershell
& "C:\Program Files\IIS Express\iisexpress.exe" /path:"$PWD\KMCEvent.Api" /port:44356
```

5. Verify backend is running:
   - `http://localhost:44356/API.asmx`

### Frontend second (WinForms)

1. Open a second VS Code terminal.
2. Build frontend project:

```powershell
msbuild .\KMCEvent.Client\KMCEvent.Client.csproj /t:Build /p:Configuration=Debug
```

3. Run frontend app:

```powershell
.\KMCEvent.Client\bin\Debug\KMCEvent.Client.exe
```

4. Login using seeded users (password `1234`), for example:
   - `organizer1`
   - `participant1`
   - `public1`

## 5. Common issue check

If login shows service connection errors:
1. Confirm backend terminal is still running.
2. Confirm `http://localhost:44356/API.asmx` is reachable in browser.
3. Confirm `KMCEvent.Client/App.config` endpoint matches backend URL and port exactly.
