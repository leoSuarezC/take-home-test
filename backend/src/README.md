## Running the Backend

To build the backend, navigate to the `src` folder and run:  
```sh
dotnet build
```

To run all tests:  
```sh
dotnet test
```

To start the main API:  
```sh
cd Fundo.Applications.WebApi  
dotnet run
```

The following endpoint should return **200 OK**:  
```http
GET -> http://localhost:8080/loans
```

For full setup instructions (Docker, frontend, API reference), see the [root README](../../README.md).

## Notes  

Feel free to modify the code as needed, but try to **respect and extend the current architecture**, as this is intended to be a replica of the Fundo codebase.
