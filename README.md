# FormsTicketTool

> decrypt / encrypt / create **ASP.NET FormsAuthentication** tickets using leaked machineKeys.

## Overview

A small Windows tool for decrypt/encrypt/create cookie "sessions" (`FormsAuthenticationTicket `) when owning the machineKey.

---

## Build

**Requirements**

- Visual Studio (or `csc.exe` / `dotnet build`)
- .NET Framework 4.8
- Add reference: `System.Web`

**Steps**

1. Clone the repo:
   ```bash
   git clone https://github.com/4xura/FormsTicketTool.git
   cd FormsTicketTool
   ```

2. Open `FormsTicketTool.sln` in Visual Studio or compile manually:

   ```
   csc /r:System.Web.dll FormsTicketTool.cs
   ```

3. Copy `App.config.template` → `App.config`. Insert your target machineKey:

4. Build → `FormsTicketTool.exe` will appear in `/bin/Debug` or `/bin/Release`.

---

## Usage

```
FormsTicketTool.exe decrypt <COOKIE> [--json] [--utc]
FormsTicketTool.exe encrypt <EXISTING_COOKIE> <NEW_USER> <USERDATA> <MINUTES_VALID>
FormsTicketTool.exe create  <USERNAME> <USERDATA> <MINUTES_VALID> [isPersistent]
FormsTicketTool.exe gen-config <decryptionKey> <validationKey> [compatibilityMode] [outfile]
FormsTicketTool.exe info
```

Prints:

- default: TTL and local timestamps
- `--json` (optional): machine-friendly JSON
- `--utc`  (optional): show timestamps in UTC instead of local

Examples:

```sh
# decrypt a cookie string
FormsTicketTool.exe decrypt 8CE190CA4584C2E0...

# clone cookie into admin with Administrators role
FormsTicketTool.exe encrypt 8CE190CA4584C2E0... admin "Administrators" 120
```

------

## Config Example

`App.config.template` (rename to `App.config` for actual run):

```xml
<configuration>
  <system.web>
    <machineKey
      decryption="AES"
      decryptionKey="B26C371E..."
      validation="HMACSHA256"
      validationKey="EBF9076B..."
      compatibilityMode="Framework45" />
  </system.web>
</configuration>
```

------

## Disclaimer

This project is for educational and authorized research only.