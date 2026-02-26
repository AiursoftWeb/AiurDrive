# Gemini Changelog

## Feature: Private Site Sharing

Implemented the ability to share private sites with specific users or roles.

### Changes:

1.  **Entities**:
    *   Added `SiteShare` entity to store sharing information.
    *   Added `SharePermission` enum (ReadOnly, Editable).
    *   Updated `Site` entity to include `SiteShares` collection.
    *   Updated `AiurDriveDbContext` to include `SiteShares` DbSet.

2.  **Database**:
    *   Added migrations for SQLite and MySQL (`AddSiteShares`).

3.  **Controllers**:
    *   **SharesController**: Created to handle:
        *   `Manage(siteName)`: View sharing settings.
        *   `AddShare(siteName, model)`: Add a new share.
        *   `RemoveShare(id)`: Remove a share.
        *   `SharedWithMe()`: List sites shared with the current user.
    *   **DashboardController**: Updated to check for shared access permissions in:
        *   `Files` (View files).
        *   `CreateFolder`, `Delete`, `Rename`, `Move` (Modify files).
        *   Added `HasAccess` helper method to verify permissions based on ownership and shares.
        *   Added "Manage Shares" button to the File Manager sidebar.
    *   **UsersController**: Added `Search` action to support user autocomplete in the sharing UI.

4.  **ViewModels**:
    *   Added `ManageSharesViewModel`.
    *   Added `AddShareViewModel`.
    *   Added `SharedWithMeViewModel`.

5.  **Views**:
    *   Added `Views/Shares/Manage.cshtml`.
    *   Added `Views/Shares/SharedWithMe.cshtml`.
    *   Updated `Views/Dashboard/Files.cshtml` to include the entry point for managing shares.

6.  **UI Improvements**:
    *   The "Manage Shares" button is only visible to the site owner in the File Manager.

### Key Logic:

*   **Access Control**: If a user is not the owner of a site, the system checks the `SiteShares` table to see if the user (or one of their roles) has been granted access.
*   **Permissions**: 
    *   `ReadOnly`: Allows viewing files and listing directories.
    *   `Editable`: Allows creating folders, uploading (handled via existing logic + permission check), renaming, moving, and deleting files/folders.
*   **UI**:
    *   Users can search for other users by name/email to share with.
    *   Users can select roles to share with.
    *   "Shared with me" page lists all sites shared with the user.

## Optimization: Refactored Storage Operations (Issue #34)

Centralized and optimized file system operations to improve security, performance, and maintainability.

### Changes:

1.  **StorageService**:
    *   Refactored `GetDirectorySize` to use a non-recursive approach with `Stack<DirectoryInfo>` and `EnumerateFiles/EnumerateDirectories` for better performance on large/deep directory trees and reduced memory footprint.
    *   Added centralized methods: `CreateDirectory`, `DeleteFileOrDirectory`, `RenameFileOrDirectory`, and `MoveFileOrDirectory` with built-in path traversal security checks and exception handling.
    *   Improved `Save` method by correctly disposing `FileStream` objects.

2.  **Controllers**:
    *   Updated `DashboardController` and `SharedViewController` to utilize the new centralized `StorageService` methods, reducing manual path manipulation and ensuring consistent security checks across the application.
    *   Wrapped several hardcoded error messages in `localizer` to facilitate future localization.

3.  **Security**:
    *   Ensured all file system operations leverage the centralized path traversal detection in `StorageService`.
