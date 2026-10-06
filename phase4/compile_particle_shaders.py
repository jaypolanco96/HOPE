"""Compile only the isolated particle shader; never rewrite scene/cloth shaders."""
import pathlib, struct, subprocess, sys, tempfile
root=pathlib.Path(__file__).resolve().parents[1]
output=['// Generated isolated particle shaders; DXC v1.9.2609.','#pragma once','#include <cstdint>','namespace hope::particle_spirv {']
with tempfile.TemporaryDirectory() as td:
 for hdr in (False,True):
  base=[sys.argv[1],str(root/'src/native/shaders/particle.hlsl'),'-E','ps_main','-T','ps_6_0','-O3']
  if hdr:base += ['-D','HDR=1']
  subprocess.run(base+['-Fo',str(pathlib.Path(td)/'check.dxil')],check=True)
  out=pathlib.Path(td)/'check.spv'
  subprocess.run(base+['-spirv','-fspv-target-env=vulkan1.1','-fvk-bind-register','t0','0','0','1','-fvk-bind-register','s1','0','5','0','-Fo',str(out)],check=True)
  data=out.read_bytes(); words=struct.unpack('<'+'I'*(len(data)//4),data)
  name='hdr' if hdr else 'gamma'
  output.append('inline constexpr uint32_t '+name+'[] = {')
  output += ['  '+', '.join(f'0x{w:08x}' for w in words[i:i+8])+',' for i in range(0,len(words),8)]
  output.append('};')
  print(name,len(data),'bytes; D3D12 and Vulkan compiled')
output.append('}')
(root/'src/native/shaders/spirv/hope_particle_spirv.h').write_text('\n'.join(output)+'\n')
for path in ['src/native/shaders/scene.hlsl','src/native/shaders/spirv/skate3_native_shaders_spirv.h']:
 baseline=subprocess.check_output(['git','show','f583fcc:'+path],cwd=root)
 assert (root/path).read_bytes().replace(b'\r\n',b'\n')==baseline.replace(b'\r\n',b'\n'), path+' changed'
print('Original scene and cloth shaders unchanged from pre-particle build')
