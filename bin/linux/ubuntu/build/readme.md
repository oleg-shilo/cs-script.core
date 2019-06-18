For any new build:

(for "how to prepare package builder" see ..\build.readme.md)

1. Build and pack the release

2. Prepare package sources by executing `prepare_package_sources.cs`. It will in turn:
    - update `install` with the location of the path to the files to be included in the package
    - update `changelog` with the new version and release notes

3. Copy entire `build` folder on Ubuntu. <br>
    IMPORTANT: do not copy files via VMware tools (drag-n-drop) as it screws the files. Instead:

    - zip the build directory (done by `prepare_package_sources.cs`)
    - copy (drag-n-drop) build.zip file to the guest system
    - unzip files on the guest system

4. From the build folder in the terminal execute `debuild -b` or `debuild -b -d -uc -us` if you are OK with unsigned package

    - Test the package:
        - sudo dpkg --purge cs-script.core
        - sudo dpkg -i ../cs-script.core_*.deb  (or specify the version instead of *)

5. Copy cs-script*.deb package back to Win

6. Commit to the GitHub


------------------------------

`css` is a shell script with the following content

```
#! /bin/sh
dotnet /home/user/Desktop/dev/dotnet.core/build.core/cs-script.core_1.1.2.0/cscs.dll "$@"
```

It is placed in `/usr/local/bin` and changed to executable:
```
cp <css_install_dir>/css /usr/local/bin/css
chmod +x /usr/local/bin/css
```

Now you can execute scripts the same way as on Windows:

```
css -new script.cs
css script
```

Install command:
```
repo=http://www.cs-script.net/cs-script.core/linux/ubuntu/; file=$(echo cs-script.core_)$(curl -L $repo/version.txt --silent)$(echo _all.deb); rm $file; wget $repo$file; sudo dpkg -i $file
```

Uninstall command:

```
css -update -u

# or
dpkg --purge cs-script.core
```