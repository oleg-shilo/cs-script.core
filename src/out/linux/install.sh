# symbolic link for css
# ln -s /usr/local/bin/cs-script/cscs.exe /usr/local/bin/cs-script/css
# sudo chmod +rwx /usr/local/bin/cs-script/css

sudo chmod +x /usr/local/bin/cs-script/css

#PATH to bash envar
echo 'export CSSCRIPT_DIR=/usr/local/bin/cs-script' >> ~/.bashrc
echo 'export PATH=$PATH:/usr/local/bin/cs-script' >> ~/.bashrc

