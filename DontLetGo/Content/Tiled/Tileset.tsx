<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.2" tiledversion="1.3.4" name="Tileset" tilewidth="16" tileheight="16" tilecount="64" columns="8">
 <image source="../Textures/Tiles.png" width="128" height="128"/>
 <tile id="0">
  <properties>
   <property name="Walkable" type="bool" value="true"/>
  </properties>
 </tile>
 <tile id="1">
  <objectgroup draworder="index" id="3">
   <object id="2" name="Hull" x="0" y="0" width="16" height="16"/>
  </objectgroup>
 </tile>
 <tile id="2">
  <properties>
   <property name="Walkable" type="bool" value="true"/>
  </properties>
 </tile>
 <tile id="3">
  <properties>
   <property name="Light" type="float" value="6"/>
  </properties>
 </tile>
 <tile id="4">
  <properties>
   <property name="Activator" type="bool" value="true"/>
   <property name="ActiveState" type="int" value="6"/>
   <property name="Light" type="float" value="4"/>
   <property name="LightColor" type="color" value="#ffff70e5"/>
  </properties>
 </tile>
 <tile id="5">
  <properties>
   <property name="Light" type="float" value="2.5"/>
  </properties>
 </tile>
 <tile id="8">
  <properties>
   <property name="Walkable" type="bool" value="true"/>
  </properties>
 </tile>
 <tile id="16">
  <properties>
   <property name="Light" type="float" value="1.5"/>
   <property name="Walkable" type="bool" value="true"/>
  </properties>
 </tile>
</tileset>
