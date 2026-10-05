#include "hope_crowd_mods.h"
#include <iostream>
#include <limits>
#include <stdexcept>
using skate3::native_scene::DrawItem;
int main() {
  int checks=0; auto check=[&](bool ok,const char* label){if(!ok)throw std::runtime_error(label);++checks;};
  DrawItem ped{};ped.char_family=3;ped.lw_entity=123;ped.skinned=true;
  ped.bones={1,0,0,10,0,1,0,2,0,0,1,20, 1,0,0,10,0,1,0,0,0,0,1,20};
  auto player=ped;player.char_family=2; auto car=ped;car.char_family=6;
  auto unmapped=ped;unmapped.lw_entity=0;auto hair=ped;hair.char_family=5;
  std::vector<DrawItem> original{ped,player,car,unmapped,hair};
  auto items=original;hope::ApplyCrowdStyle(items,0);
  check(items[0].bones==ped.bones && !items[0].hope_neon_crowd,"Default has no effect");
  hope::ApplyCrowdStyle(items,1);
  check(items[0].hope_neon_crowd && items[4].hope_neon_crowd,"Neon affects mapped body and hair");
  check(!items[1].hope_neon_crowd && !items[2].hope_neon_crowd && !items[3].hope_neon_crowd,"Player, vehicles, unmapped stay original");
  check(items[0].bones==ped.bones,"Neon does not alter animation");
  items=original;hope::ApplyCrowdStyle(items,2);
  check(items[0].bones[0]==0.5f && items[0].bones[7]==1 && items[0].bones[19]==0,"Pocket shrinks animation around feet");
  check(items[4].bones==items[0].bones,"Body and hair share entity anchor");
  check(items[1].bones==player.bones && items[2].bones==car.bones && items[3].bones==unmapped.bones,"Scale leaves non-target palettes untouched");
  items=original;hope::ApplyCrowdStyle(items,3);
  check(items[0].bones[0]==2 && items[0].bones[7]==4 && items[0].bones[19]==0,"Giant doubles animation above feet");
  check(original[0].bones==ped.bones,"Source frame remains unchanged");
  auto invalid=ped;invalid.bones[0]=std::numeric_limits<float>::quiet_NaN();items={invalid};hope::ApplyCrowdStyle(items,3);
  check(std::isnan(items[0].bones[0]) && items[0].bones[7]==2,"Non-finite palette remains untouched");
  invalid=ped;invalid.bones.resize(11);items={invalid};hope::ApplyCrowdStyle(items,2);
  check(items[0].bones.size()==11 && items[0].bones[0]==1,"Truncated palette rejected");
  items=original;hope::ApplyCrowdStyle(items,99);check(items[0].bones==ped.bones,"Unknown style is inert");
  float constants[52]={};constants[16]=7;hope::NeonTint(constants);
  check(constants[33]>0 && constants[35]==1 && constants[16]==7,"Neon preserves skinning and unrelated constants");
  items=original;hope::ApplyCrowdStyle(items,2);items=original;hope::ApplyCrowdStyle(items,2);
  check(items[0].bones[0]==0.5f,"Fresh frame changes cannot compound");
  std::cout<<"Passed "<<checks<<" render-only crowd checks.\n";
}
