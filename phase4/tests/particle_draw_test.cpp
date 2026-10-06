#include "hope_particle_draw.h"
#include <iostream>
#include <limits>
#include <stdexcept>
#include <vector>
int main() {
  int checks=0;
  auto check=[&](bool ok,const char* label){if(!ok)throw std::runtime_error(label);++checks;};
  std::vector<hope::SpriteVertex> sprite={
    {{0,0,0},{0,0}},{{.04f,0,0},{1,0}},
    {{.04f,.04f,0},{1,1}},{{0,.04f,0},{0,1}}};
  auto accept=[](const auto& v){return hope::IsSpriteBatch(uint32_t(v.size()),[&](uint32_t i){return v[i];});};
  check(accept(sprite),"Small full-UV sprite accepted");
  auto cloth=sprite;cloth[2].uv[0]=.5f;
  check(!accept(cloth),"Continuous garment UV rejected");
  cloth=sprite;for(auto& v:cloth)for(float& p:v.p)p*=20;
  check(!accept(cloth),"Garment-sized full-UV panel rejected");
  auto batch=sprite;batch.insert(batch.end(),cloth.begin(),cloth.end());
  check(!accept(batch),"Every quad must pass, including trailing cloth");
  auto invalid=sprite;invalid[2].p[0]=std::numeric_limits<float>::quiet_NaN();
  check(!accept(invalid),"Invalid simulated position rejected");
  invalid=sprite;invalid[1].uv[0]=std::numeric_limits<float>::infinity();
  check(!accept(invalid),"Invalid UV rejected");
  invalid=sprite;invalid[1].uv[0]=0;
  check(!accept(invalid),"Repeated UV corner rejected");
  invalid=sprite;invalid[1].p[0]=0;
  check(!accept(invalid),"Degenerate geometry rejected");
  invalid=sprite;invalid.pop_back();
  check(!accept(invalid),"Partial quad rejected");
  check(!accept(std::vector<hope::SpriteVertex>{}),"Empty batch rejected");
  for(auto path:{"cac_cloth_ropa_defaultVS.updb","defaultcharacter_ropa_defaultPS.updb", "character.cloth_ropa", "cac_hair_defaultPS.updb"})
    check(hope::IsGarmentShader(path),"Known garment shader excludes dust override");
  check(!hope::IsGarmentShader("sprite_defaultPS.updb"),"Unrelated shader is not classified as a garment");
  std::cout<<checks<<" particle/cloth isolation checks passed\n";
}
