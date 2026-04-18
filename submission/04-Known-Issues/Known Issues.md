# Known Issues

## Submitter Details
- The About page still contains placeholder submitter names and IDs. Replace them before final submission.

## Database Packaging
- The project creates and seeds the LocalDB database at runtime with `EnsureCreated()`.
- The required `.dacpac` export still needs to be produced from Visual Studio SQL tooling and copied into `submission/03-Database`.

## Replay Verification
- Replay implementation is being completed separately. Re-run the full solution smoke test after the replay slice is merged.

## Notes
- No known build errors at the moment.
- The server gameplay API smoke test passed: session creation, session fetch, and human move submission all worked successfully.

## Submitters
- Submitter 1: replace before delivery
- Submitter 2: replace before delivery or mark `N/A`
