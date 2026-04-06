# Task 3: Testing and Debugging

## 1. Comprehensive Testing Strategies
To ensure the reliability, functionality, and performance of the Service-Oriented Computing application, a comprehensive testing strategy is implemented across multiple layers of the application.

*   **Unit Testing**: Isolated testing is performed on individual functions and methods within the API layer (e.g., methods in `OrganizerActions`, `ParticipantActions`, and `DatabaseManager`). This verifies that business logic algorithms process inputs and return expected outputs correctly without involving the database or network.
*   **Integration Testing**: This tests the interaction between different modules. Specifically, we test the communication between the `KMCEvent.Client` application and the `KMCEvent.Api` web service over SOAP/HTTP, as well as the connection between the API and the underlying SQL Database via the `DatabaseManager`.
*   **Functional Testing**: End-to-end testing validates the software against functional requirements. This involves interacting with the Windows Forms UI (`LoginWindow`, `OrganizerDashboard`, `ParticipantDashboard`) to simulate real user scenarios, such as creating events and registering for them, to ensure the system behaves as expected from a user's perspective.

## 2. Debugging Process
The debugging process is structured to resolve issues efficiently and isolate faults between the client and service layers:
1.  **Replication**: Reproducing the issue using the Client UI to understand the exact conditions of the bug.
2.  **Log and Exception Inspection**: Reviewing error messages caught by `try-catch` blocks in the client application or SOAP fault exceptions returned by the API.
3.  **Breakpoints and Stepping**: Utilizing Visual Studio's debugger. Placing breakpoints in `API.asmx.cs` and the Controller classes (e.g., `EventCatalogActions`) to inspect variables, monitor request payloads, and track the execution flow.
4.  **Database Tracing**: Verifying if the data anomalies originate from the SQL execution by executing the generated queries directly in SQL Server Management Studio (SSMS).
5.  **Resolution and Re-testing**: Applying the fix and re-running the associated Functional and Integration tests to ensure the issue is resolved and no regressions are introduced.

## 3. Test Results and Test Cases

Below are the detailed test cases and results executed on the system.

### Test Case 1
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-001 |
| **Test case name** | Valid User Login |
| **Test steps** | 1. Open `LoginWindow`<br>2. Enter valid username<br>3. Enter valid password<br>4. Click Login button |
| **Test data** | Username: `org_admin`<br>Password: `Pass123` |
| **Expected result** | System validates credentials, identifies role as Organizer, and opens `OrganizerDashboard`. |
| **Actual result** | System validated credentials and successfully opened the Organizer Dashboard. |
| **Remarks** | Pass |

### Test Case 2
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-002 |
| **Test case name** | Invalid User Login |
| **Test steps** | 1. Open `LoginWindow`<br>2. Enter correct username<br>3. Enter incorrect password<br>4. Click Login button |
| **Test data** | Username: `org_admin`<br>Password: `WrongPass` |
| **Expected result** | System denies access and displays an "Invalid Username or Password" error message. |
| **Actual result** | Error message displayed; access denied. |
| **Remarks** | Pass |

### Test Case 3
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-003 |
| **Test case name** | Create New Event (Organizer) |
| **Test steps** | 1. Login as Organizer<br>2. Navigate to Create Event section<br>3. Fill in title, description, and date<br>4. Click Create Event |
| **Test data** | Title: `Annual Tech Meetup`<br>Description: `Networking event.`<br>Date: `2026-05-10` |
| **Expected result** | API processes request, inserts record into Database, and UI shows "Event Created Successfully". |
| **Actual result** | Event created and stored in the database correctly. |
| **Remarks** | Pass |

### Test Case 4
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-004 |
| **Test case name** | Empty Field Validation on Event Creation |
| **Test steps** | 1. Login as Organizer<br>2. Navigate to Create Event<br>3. Leave Title blank<br>4. Click Create Event |
| **Test data** | Title: `[Empty]`<br>Description: `Test description.` |
| **Expected result** | UI validation prevents submission and alerts the user that Title is required. |
| **Actual result** | Submission blocked, validation alert displayed. |
| **Remarks** | Pass. Handled at client-side to save API processing. |

### Test Case 5
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-005 |
| **Test case name** | View Event Catalog (Organizer) |
| **Test steps** | 1. Login as Organizer<br>2. Click on "View My Events" |
| **Test data** | Organizer ID: `1` |
| **Expected result** | System fetches only the events created by this specific organizer from `EventCatalogActions` and binds to the GridView. |
| **Actual result** | Grid filled with the correct filtered list of events. |
| **Remarks** | Pass |

### Test Case 6
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-006 |
| **Test case name** | View Public Events (Participant) |
| **Test steps** | 1. Login as Participant<br>2. Navigate to `PublicEventsDashboard` |
| **Test data** | None |
| **Expected result** | System retrieves all upcoming public events and displays them in a selectable list. |
| **Actual result** | List successfully populated with upcoming public events. |
| **Remarks** | Pass |

### Test Case 7
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-007 |
| **Test case name** | Register for an Event (Participant) |
| **Test steps** | 1. Login as Participant<br>2. Select an event from Public Events<br>3. Click Register |
| **Test data** | User ID: `5`<br>Event ID: `10` |
| **Expected result** | API records the registration in the database and UI shows "Registration Successful". |
| **Actual result** | Registration successful, database updated. |
| **Remarks** | Pass |

### Test Case 8
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-008 |
| **Test case name** | Prevent Duplicate Registration |
| **Test steps** | 1. Login as Participant<br>2. Select an event already registered for<br>3. Click Register |
| **Test data** | User ID: `5`<br>Event ID: `10` (already registered) |
| **Expected result** | API checks existing records, rejects registration, and UI shows "You are already registered for this event." |
| **Actual result** | Error message displayed as expected; no duplicate row in DB. |
| **Remarks** | Pass. Business logic prevents duplicates. |

### Test Case 9
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-009 |
| **Test case name** | View Registrations for an Event (Organizer) |
| **Test steps** | 1. Login as Organizer<br>2. Select one of own events<br>3. Click View Participants |
| **Test data** | Event ID: `10` |
| **Expected result** | System fetches and displays a list of all participants registered for that specific event. |
| **Actual result** | Correct list of participants displayed. |
| **Remarks** | Pass |

### Test Case 10
| Field | Details |
| :--- | :--- |
| **Test case id** | TC-010 |
| **Test case name** | Integration: API Connection Failure Handling |
| **Test steps** | 1. Stop the IIS Express / Web API server.<br>2. Open Client Application<br>3. Attempt to Login |
| **Test data** | Valid Credentials |
| **Expected result** | Client application gracefully handles the `EndpointNotFoundException`, showing a friendly "Unable to connect to server" message instead of a raw crash. |
| **Actual result** | Friendly connection error message displayed. Application continued running. |
| **Remarks** | Pass. Client handles external dependencies safely. |