# xor
XOR encryption of files in C#

#xor.exe
- XOR encrypt/decrypt entire file or first kilo byte
- accepts wildcards and multiple filenames
- if no key is provided and /q is specified, will apply (255-byte) to first 1k byte
`xor.exe ww.jpg /q == 1k.exe ww.jpg`

##usage samples
`
    xor.exe /?
    XOR encrypt/decrypt files.
    usage:  xor.exe /key:<encryption/decryption key> [/1k] [/v] filename1 [filename2 ...]
    If /key is not provided and /quick is present, the first 1024 bytes will be changed to (255-byte)
    /?     /h /help                Display program help
    /k:    /key:                   <string> key used in encryption or decryption
    /v     /verbose                Verbose progress messages
    /1k    /1024 /q /quick         Process only first 1024 bytes of the file
`

`xor.exe test.jpg /q`

`xor.exe *.jpg /q`

`xor.exe *.mp4 *.jpg /quick /key:something`

#IMPORTANT
If "xor.exe" is renamed to "1k.exe" it will ignore all switches and will only perform 1k:(255-byte)




