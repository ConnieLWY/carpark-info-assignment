## Key Features
* Daily CSV Import: Automated background job to refresh carpark data
* ERD Diagram: Detailed database schema available
* Two-Tier API Access:
  - Public API (V1): Basic operations
  - Authenticated API (V2): Advanced features with token-based security

## System Components
### ERD Diagram
* Detailed database schema available in ERD Diagram.png

### Batch Job
* Automatic daily CSV file import
* Background refresh mechanism
* Error handling and logging

## API Endpoints
### Carpark API V1 (Public)
* Check CSV import logs
* User registration
* User deletion
* User retrieval

### Carpark API V2 (Authenticated)
#### Authentication Workflow
1. Login
  * Default credentials:
    * Username: user1
    * Password: user1

2. Authorization
  * Obtain authentication token
  * Use format: Bearer [token]

3. Token Validation
  * Verify authentication status

#### Authentication Workflow
* Query Carparks
* Set Favorite Carparks
* Remove Favorites
* Check Favorite Carparks

## Running the Project
### In Visual Studio 2022
1. Open the solution
2. Press F5 or click the "Start" button (green play button)
3. Project will build and launch in default browser
