# Security policy

Do not place secrets or private evidence in a public issue, pull request, commit, workflow input, log, or artifact.

Use GitHub private vulnerability reporting when it is enabled for this repository. If it is unavailable, contact the repository owner through a private, previously verified channel before sharing details. Do not create a public placeholder containing credentials, exploit material, customer data, or internal endpoints.

Apple certificates, provisioning profiles, App Store Connect keys, and their passwords belong only in the protected `testflight-canary` GitHub environment. Rotate any credential immediately if it is exposed, even when GitHub masks the displayed value.
