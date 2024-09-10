# TODO

Once these changes are complete, move the entries to the [changelog](./changelog.md).

- [ ] Argument Completion
  - [ ] WindowsUpdate
    - [ ] title
    - [ ] update ID
    - [ ] KB article id
    - [ ] `Get-WindowsUpdate`
    - [ ] `Get-WindowsUpdateHistory`
    - [ ] `Install-WindowsUpdate`
    - [ ] `Download-WindowsUpdate`
  - [ ] WindowsUpdateSession
- [ ] Commands
  - [ ] `Download-WindowsUpdate`
  - [ ] `Show-WindowsUpdate`
  - [ ] `Hide-WindowsUpdate`
  - [ ] `Export-WindowsUpdate`
  - [ ] `Import-WindowsUpdate`
- [ ] simplify internals
  - [ ] `Get-WindowsUpdate` search job
  - [ ] do we gain anything from Dependency Injection?
  - [ ] consolidate API used by sync and async native update operations
    - this may be easiest if done by making the cmdlets support async
      - this would be dependent on removing DI or utilizing a helper method to turn a sync cmdlet method into an async one
- [ ] timeout
  - [ ] `Get-WindowsUpdate`
  - [ ] `Install-WindowsUpdate`
  - [ ] `Download-WindowsUpdate`
- [ ] dynamically `-JobName` parameter
  - [ ] `Get-WindowsUpdate`
  - [ ] `Install-WindowsUpdate`
  - [ ] `Download-WindowsUpdate`
