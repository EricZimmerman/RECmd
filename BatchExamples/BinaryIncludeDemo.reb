Description: BinaryInclude demo
Author: Eric Zimmerman
Version: 1
Id: 1e144fa4-70ca-478f-b0b9-a148e4ba1b90
Keys:
    -
        Description: demo
        HiveType: SYSTEM
        Category: System Info
        KeyPath: ControlSet001\Control\NetworkSetup2\Interfaces\*\Kernel
        ValueName: CurrentAddress
        IncludeBinary: true
        BinaryConvert: IP

# IncludeBinary: true
# if true, include bytes vs showing (binary data) in format 0A-11-AB-09

# BinaryConvert: IP
# if present, convert binary data to either Ipv4 address (IP) or 64 bit FILETIME (FILETIME)
# Conversion happens from offset 0. if an error happens, the unconverted data is included
