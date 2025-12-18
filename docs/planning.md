### Done

| Done | Task                                                                        | Time  |
|------|-----------------------------------------------------------------------------|-------|
| [x]  | Verify SettingsManager work via swagger                                     | 30m   |
| [x]  | Fix found errors                                                            | 1h?   |
| [x]  | Verify ChannelReader logic via swagger                                      | 30m   |
| [x]  | Learn Syndication and implement it                                          | 1h    |
| [x]  | Check db schema and/or fix it                                               | 30m   |
| [x]  | Implement repositories                                                      | 2h    |
| [x]  | Fix db initialization                                                       | 1h    |
| [x]  | Add openai docs to CodeGPT                                                  | 30min |
| [x]  | Implement basic summariser                                                  | 30min |
| [x]  | Add Razor Pages UI draft                                                    | 1h    |
| [x]  | Fix compile issues in UI                                                    | 1h    |
| [x]  | Learn Razor by TODOs                                                        | 1h    |
| [x]  | Fix error handling                                                          | 1h    |
| [x]  | Fix Digests UI Page                                                         | 1h    |
| [x]  | Fix Digest UI Page                                                          | 1h    |
| [x]  | Fix Channels UI Page                                                        | 1h    |
| [x]  | Fix Settings UI Page                                                        | 1h    |
| [x]  | Learn Razor binding                                                         | 30min |
| [x]  | Move prompt to settings                                                     | 30min |
| [x]  | Add all settings fields in UI                                               | 30min |
| [x]  | Use Backend types in VMs                                                    | 1h    |
| [x]  | Digest publishing to RSS, RSS Validation via https://validator.w3.org/feed/ | 1h    |
| [x]  | Error handling: redirect to base page from handlers (eg add)                | 1h    |
| [x]  | Bug: channel deletion                                                       | 30min |
| [x]  | Add BaseUrl                                                                 | 2h    |
| [x]  | Digest generation: Digest Progress page                                     | 2h    |
| [x]  | Digest generation: Async queue                                              | 3h    |
| [x]  | digest deletion                                                             | 6h    |
| [x]  | Digest generation: Digest generation status service                         | 3h    |
| [x]  | Digest generation: display errors in progress                               | 1h    |
| [x]  | Digest generation: show steps in progress                                   | 1h    |
| [x]  | Digest generation: fix progress                                             | 1h    |
| [x]  | Clean logs                                                                  | 30min |
| [x]  | Digest generation: Parametrized                                             | 3h    |
| [x]  | Any RSS: Handle any rss feed in SummaryGenerator                            | 1h    |
| [x]  | Any RSS: Add any RSS feed in UI                                             | 1h    |
| [x]  | Any RSS: rename digest UI items                                             | 1h    |
| [x]  | auto apply runtime argument null checks via code generation on compilation  | 1h    |
| [x]  | Error handling: exception stacktrace in UI                                  | 30min |
| [x]  | Error handling: use Result in frontend                                      | 1h    |
| [x]  | Check new tests done by ai                                                  | 30min |
| [x]  | Simplify env vars handling                                                  | 30min |
| [x]  | Simplify prompts                                                            | 30min |
| [x]  | Replace "Back to digests" with "Back"                                       | 30min |
| [x]  | Load tg rss providers from options                                          | 30min |
| [x]  | Override prompt in generate UI                                              | 1h    |
| [x]  | Tests: unit                                                                 | 2h    |
| [x]  | Move prompts from settings to table                                         | 1h    |
| [x]  | Obtain API key and pay                                                      | 30min |
| [x]  | Faster docker build                                                         | 1h    |
| [x]  | Configure default settings via options                                      | 1h    |

### High priority

| Done | Task                                                      | Time  |
|------|-----------------------------------------------------------|-------|
| [ ]  | Bug: newlines in UI                                       | 30min |
| [ ]  | Bug: hanging on feed addition, add timeout?               | 30min |
| [ ]  | Bug: generation cancels for unknown reason                | 30min |
| [ ]  | Bug: spawn multiple generations and see strange stuff     | 30min |
| [ ]  | Bug: use PostSummaries to calculate average importance    | 30min |
| [ ]  | Bug: add new chanel: backend error not shown until reload | 30min |
| [ ]  | Limit max string length of prompts in VM level            | 30min |
| [ ]  | Support reading complex media or links from RSS           | 30min |
| [ ]  | Digest generation: queue monitor                          | 3h    |
| [ ]  | Digest generation: cancellation                           | 1h    |
| [ ]  | Reimplement and test email sending?                       | 2h    |
| [ ]  | Configure deploy                                          | 4h    |

### Low priority

| Done | Task                                                   | Time  |
|------|--------------------------------------------------------|-------|
| [ ]  | Add more parameters and jinja template to prompts      | 1h    |
| [ ]  | Use OneOf in auth options                              | 1h    |
| [ ]  | Use service instead of middleware for user persistence | 1h    |
| [ ]  | Digest generation result: SuccessButSomeFeedsFailed    | 2h    |
| [ ]  | Add XML docs in existing code                          | 1h    |
| [ ]  | Error handling: 404 page                               | 30min |
| [ ]  | Tests: integration                                     | 2h    |
| [ ]  | Tests: UI                                              | 2h    |
| [ ]  | Tests: validators                                      | 2h    |
| [ ]  | Allow empty API key                                    | 1h    |
| [ ]  | Support cancellation everywhere                        | 1h    |
| [ ]  | LiveBDD                                                | 1h    |
| [ ]  | Add params like MaxToken to settings (via json field?) | 1h    |

### Future

- improve openapi/swagger support in api controller
    - fix error codes
        - 404 / NotFoundError, OneOf?
- Check code quality
- use Optional instead of nulls
- use arrays instead of Lists if data is immutable
- add users and auth
- improve error handling by adding error "location" to UI
