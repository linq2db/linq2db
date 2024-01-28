import bpy

# Clean the scene
#
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

bpy.ops.outliner.orphans_purge()
bpy.ops.outliner.orphans_purge()
bpy.ops.outliner.orphans_purge()

for material in bpy.data.materials:
    bpy.data.materials.remove(material)

bpy.context.scene.eevee.use_ssr = True

# Create materials
##
def create_material(name, r, g, b) :
    m = bpy.data.materials.new(name)
    
    m.use_nodes = True
    
    nodes = m.node_tree.nodes
    
    nodes["Principled BSDF"].inputs[0].default_value = (r, g, b, 1) # Base Color
    nodes["Principled BSDF"].inputs[1].default_value = 1            # Metalic
    nodes["Principled BSDF"].inputs[2].default_value = 0.2          # Roughness
    
    bump_node = nodes.new(type="ShaderNodeBump")
    
    bump_node.inputs[0].default_value = 0.2 # Strength
    bump_node.inputs[1].default_value = 0.1 # Distance
    
    m.node_tree.links.new(bump_node.outputs["Normal"], nodes.get("Principled BSDF").inputs["Normal"])
    
    tex_noise = nodes.new(type="ShaderNodeTexNoise")

    tex_noise.inputs[2].default_value = 500 # Scale
    tex_noise.inputs[3].default_value = 10  # Detail
    tex_noise.inputs[4].default_value = 1   # Roughness

    m.node_tree.links.new(tex_noise.outputs["Fac"],  bump_node.inputs["Height"])
    
    return m

m_base = create_material('m_base', 0.175, 0.45, 0.8)
m_cyl  = create_material('m_cyl',  1,     1,    1)
        
location = (0, 0, 0)
scale    = 1

# Create cylinders
#
def create_cylinder(name, mat, lz, sx, sy, sz) :
    cyl = bpy.ops.mesh.primitive_cylinder_add( \
        radius         = 1,              \
        depth          = 2,              \
        enter_editmode = False,          \
        align          = 'WORLD',        \
        location       = (0  * scale + location[0], 0 * scale + location[1], lz * scale + location[2]), \
        scale          = (sx * scale, sy * scale, sz * scale))

    cyl = bpy.context.active_object

    cyl.name = 'cyl_' + name
    cyl.data.materials.append(mat)
            
    cyl.modifiers.new('EdgeSplit',   'EDGE_SPLIT')
    cyl.modifiers.new('Subdivision', 'SUBSURF')
    
    cyl.modifiers["Subdivision"].levels = 3
            
    return cyl

c_base   = create_cylinder('base',   m_base, 0,    1.0, 1.0, 1.0)
c_top    = create_cylinder('top',    m_cyl,  0.75, 1.1, 1.1, 0.3)
c_mid    = create_cylinder('mid',    m_cyl,  0,    1.1, 1.1, 0.3)
c_bottom = create_cylinder('buttom', m_cyl, -0.75, 1.1, 1.1, 0.3)

def create_light(name, x, y, z, energy, size, specular) :
    bpy.ops.object.light_add( \
        type     = 'AREA',   \
        align    = 'WORLD',  \
        location = (x * scale + location[0], y * scale + location[1], z * scale + location[2]), \
        scale    = (scale, scale, scale))

    bpy.context.object.name                 = "TriLamp-" + name
    bpy.context.object.data.energy          = energy
    bpy.context.object.data.size            = size * scale
    bpy.context.object.data.specular_factor = specular
    bpy.context.object.data.shape           = 'DISK'
    bpy.ops.object.constraint_add(type='TRACK_TO')
    bpy.context.object.constraints["Track To"].target = bpy.data.objects["cyl_base"]


create_light("Key",     5.2,  0,   5,  3000, 6, 10)
create_light("Full",    3.0,  4,   3,   500, 4, 10)
create_light("Back",   -1.5, -4.4, 5,   500, 5, 10)
create_light("Back 2", -1.5,  4.4, 5,   500, 5, 10)

bpy.ops.object.camera_add(      \
    enter_editmode = False,     \
    align          = 'VIEW',    \
    location       = (8.7 * scale + location[0], 2.3 * scale + location[1], 2.4 * scale + location[2]), \
    rotation       = (1.309, 0, 1.8326), \
    scale          = (scale, scale, scale))

bpy.ops.object.constraint_add(type='TRACK_TO')
bpy.context.object.constraints["Track To"].target = bpy.data.objects["cyl_base"]
