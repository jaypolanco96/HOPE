#include "hope_particle_texture.h"
#include <algorithm>
#include <fstream>
#include <iostream>
#include <stdexcept>
int main(int argc, char** argv) {
  int checks=0;
  auto check=[&](bool pass,const char* name){if(!pass)throw std::runtime_error(name);++checks;};
  const auto pixels=hope::MakeParticleTexture();
  constexpr auto n=hope::kParticleSize;
  auto a=[&](uint32_t x,uint32_t y){return pixels[(y*n+x)*4+3];};
  bool border=true, rgb=true; unsigned total=0,visible=0,soft=0;
  for(unsigned y=0;y<n;++y) for(unsigned x=0;x<n;++x){
    if(x==0||y==0||x==n-1||y==n-1)border &= a(x,y)==0;
    const unsigned i=(y*n+x)*4;
    rgb &= pixels[i]==218 && pixels[i+1]==210 && pixels[i+2]==194;
    total+=a(x,y);visible+=a(x,y)>0;soft+=a(x,y)>0&&a(x,y)<128;
  }
  check(border,"Every boundary texel is fully transparent");
  check(rgb,"Straight-alpha RGB continues into transparent rim to avoid dark fringes");
  check(a(n/2,n/2)>=140 && a(n/2,n/2)<=180,"Soft center has bounded opacity");
  check(a(n/2,n/2)>a(n*3/4,n/2) && a(n*3/4,n/2)>a(n-8,n/2),"Coverage fades towards rim");
  check(visible>n*n/2 && visible<n*n*4/5,"Compact disk coverage fits square without square edges");
  check(soft>visible*9/10,"Most covered texels retain a gradual soft alpha");
  check(total>n*n*15 && total<n*n*40,"Dust opacity remains sparse when overlaid");
  check(pixels==hope::MakeParticleTexture(),"Generation is deterministic");
  check(n*4%256==0,"GPU upload row pitch is aligned");
  if(argc==2){std::ofstream out(argv[1],std::ios::binary);out.write(reinterpret_cast<const char*>(pixels.data()),pixels.size());check(bool(out),"RGBA export succeeds");}
  std::cout<<checks<<" particle texture checks passed\n";
}
