# install package building tools
sudo apt-get install packaging-dev
sudo apt-get install dh-make

# build unsigned package
debuild -b -d -uc -us
