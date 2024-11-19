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

bpy.context.scene.eevee.use_ssr           = True
bpy.context.scene.render.film_transparent = True
#bpy.context.space_data.shading.use_scene_world_render = False
#bpy.context.space_data.shading.studio_light = 'sunset.exr'


# Create materials
##
def create_material(name, r, g, b, bri, con, shadow_method = 'OPAQUE') :
    m = bpy.data.materials.new(name)
    
    m.use_nodes = True
    
    nodes = m.node_tree.nodes
    
    nodes["Principled BSDF"].inputs[0].default_value    = (r, g, b, 1) # Base Color
    nodes["Principled BSDF"].inputs[1].default_value    = 1            # Metalic
    nodes["Principled BSDF"].inputs[2].default_value    = 0.2          # Roughness
    nodes["Principled BSDF"].inputs[7].default_value    = 0.02         # Weight
    nodes["Principled BSDF"].inputs[8].default_value[1] = 0.4          # Radius
    
    bc_node = nodes.new(type="ShaderNodeBrightContrast")
    
    bc_node.inputs[1].default_value = bri
    bc_node.inputs[2].default_value = con
    
    bump_node = nodes.new(type="ShaderNodeBump")
    
    bump_node.inputs[0].default_value = 0.1 # Strength
    bump_node.inputs[1].default_value = 0.2  # Distance
    
    tex_noise = nodes.new(type="ShaderNodeTexNoise")

    tex_noise.inputs[2].default_value = 2   # Scale
    tex_noise.inputs[3].default_value = 20  # Detail
    tex_noise.inputs[4].default_value = 1   # Roughness

    mapping = nodes.new(type="ShaderNodeMapping")
    
    mapping.inputs[3].default_value[2] = 200
    
    tex_coord = nodes.new(type="ShaderNodeTexCoord")

    m.node_tree.links.new(bc_node.  outputs["Color"],     nodes.get("Principled BSDF").inputs["Normal"])
    m.node_tree.links.new(bump_node.outputs["Normal"],    bc_node.  inputs["Color"])
    m.node_tree.links.new(tex_noise.outputs["Fac"],       bump_node.inputs["Height"])
    m.node_tree.links.new(mapping.  outputs["Vector"],    tex_noise.inputs["Vector"])
    m.node_tree.links.new(tex_coord.outputs["Generated"], mapping.  inputs["Vector"])

    m.shadow_method = shadow_method
    
    return m

m_base  = create_material('m_base',  1,    0.85, 0,    0,  0)
m_cyl   = create_material('m_cyl',   1,       1, 1,    4,  4.5)
m_text  = create_material('m_text',  0,       0, 0.75, 0,  0)    #, 'NONE')
m_red   = create_material('m_red',   0.75,    0, 0,    0,  0)
m_green = create_material('m_green', 0,    0.75, 0,    0,  0)
        

def create_logo(n, location, scale, text, text_loc, text_scale, cam_loc, cam_dist, light_power) :
        
    def get_location(x, y, z) :
        return ((x + location[0]) * scale, (y + location[1]) * scale, (z + location[2]) * scale)

    def get_scale(x, y, z) :
        return (x * scale, y * scale, z * scale)

    name_suffix = '_' + str(n);

    # Create cylinders
    #
    def create_cylinder(name, mat, lz, sx, sy, sz) :
        cyl = bpy.ops.mesh.primitive_cylinder_add( \
            radius         = 1,              \
            depth          = 2,              \
            enter_editmode = False,          \
            align          = 'WORLD',        \
            location       = get_location(0,  0,  lz), \
            scale          = get_scale   (sx, sy, sz))

        cyl = bpy.context.active_object

        cyl.name                 = 'cyl_' + name + name_suffix;
        cyl.data.use_auto_smooth = False
        
        cyl.data.materials.append(mat)
        cyl.active_material.metallic = 1
                
        cyl.modifiers.new('EdgeSplit',   'EDGE_SPLIT')
        cyl.modifiers.new('Subdivision', 'SUBSURF')
        
        cyl.modifiers["Subdivision"].levels = 3
        
        bpy.ops.object.shade_smooth()
                
        return cyl

    c_base   = create_cylinder('base',   m_base, 0,    1.0, 1.0, 1.0)
    c_top    = create_cylinder('top',    m_cyl,  0.75, 1.1, 1.1, 0.3)
    c_mid    = create_cylinder('mid',    m_cyl,  0,    1.1, 1.1, 0.3)
    c_bottom = create_cylinder('buttom', m_cyl, -0.75, 1.1, 1.1, 0.3)

    # Add lights
    #
    def create_light(name, x, y, z, energy, size, specular) :
        bpy.ops.object.light_add( \
            type     = 'AREA',    \
            align    = 'WORLD',   \
            location = get_location(x, y, z), \
            scale    = get_scale   (1, 1, 1))

        bpy.context.object.name                 = "TriLamp-" + name + name_suffix
        bpy.context.object.data.energy          = energy
        bpy.context.object.data.size            = size * scale
        bpy.context.object.data.specular_factor = specular
        bpy.context.object.data.shape           = 'DISK'
        
        bpy.ops.object.constraint_add(type='TRACK_TO')
        bpy.context.object.constraints["Track To"].target = c_base


    create_light("Key",     4.5,  0.5,  4, light_power[0], 6, 10)
    create_light("Full",    2,    5,    1, light_power[1], 4,  2)
    create_light("Back 1", -1.5, -4.4,  5, light_power[2], 5, 10)
    create_light("Back 2", -1.5,  4.4,  5, light_power[3], 5, 10)
    create_light("Back 3",  5,    0,   -2, light_power[4], 5, 7)

    # Add Camera
    #
    bpy.ops.object.camera_add(      \
        enter_editmode = False,     \
        align          = 'VIEW',    \
        location       = get_location(cam_loc[0] * cam_dist, cam_loc[1] * cam_dist, cam_loc[2] * cam_dist), \
        rotation       = (1.309, 0, 1.8326),          \
        scale          = get_scale(1, 1, 1))

    bpy.ops.object.constraint_add(type='TRACK_TO')
    bpy.context.object.constraints["Track To"].target = bpy.data.objects["cyl_base" + name_suffix]

    # Create λ
    #
    bpy.ops.object.text_add(          \
        enter_editmode = False,       \
        align          = 'WORLD',     \
        location       = get_location(0.95, 0,  -0.15), \
        scale          = get_scale   (3.3,  3.3, 0))

    λ = bpy.context.object
    λ.name           = "λ" + name_suffix
    λ.data.body      = "λ"
    λ.data.font      = bpy.data.fonts.load("C:\\Windows\\Fonts\\calibrib.ttf")
    λ.rotation_euler = (1.5708, 0, 1.5708)
    λ.data.align_x   = 'CENTER'
    λ.data.align_y   = 'CENTER'
    λ.scale          = get_scale(3.2, 3.2, 0.25)
    λ.data.extrude   = 1

    λ.data.materials.append(m_text)

    # Create text
    #
    bpy.ops.object.text_add(          \
        enter_editmode = False,       \
        align          = 'WORLD',     \
        location       = get_location(text_loc[0], text_loc[1], text_loc[2]), \
        scale          = get_scale   (1, 1, 1))

    l2db = bpy.context.object
    l2db.name           = text + name_suffix
    l2db.data.body      = text
    l2db.data.font      = bpy.data.fonts.load("C:\\Windows\\Fonts\\arialbd.ttf")
    l2db.rotation_euler = (1.5708, 0, 1.5708)
    l2db.data.align_x   = 'CENTER'
    l2db.data.align_y   = 'CENTER'
    l2db.data.extrude   = 0.05 * scale * text_scale
    l2db.data.size      = 0.5  * scale * text_scale

    l2db.data.materials.append(m_text)

    bpy.ops.object.convert(target='MESH')

    l2db = bpy.context.object

    mod = l2db.modifiers.new(name='Remesh', type='REMESH')
    mod.mode                    = 'SHARP'
    mod.use_remove_disconnected = False
    mod.octree_depth            = 7

    bpy.ops.object.modifier_apply(modifier="Remesh")

    mod = l2db.modifiers.new(name='Decimate', type='DECIMATE')
    mod.decimate_type = 'DISSOLVE'

    bpy.ops.object.modifier_apply(modifier="Decimate")

    bpy.ops.object.shade_smooth()
    l2db.data.use_auto_smooth = True

    bpy.ops.curve.primitive_bezier_circle_add(
        enter_editmode = False, 
        align          = 'WORLD', 
        location       = get_location(0, 0, text_loc[2]), \
        scale          = get_scale(1, 1, 1))

    circle = bpy.context.object
    circle.scale             = get_scale(1.05, 1.05, 1)
    circle.rotation_euler[2] = l2db.rotation_euler[2]

    mod = l2db.modifiers.new(name='Curve', type='CURVE')
    mod.object = circle

    bpy.ops.object.editmode_toggle()
    bpy.ops.curve.switch_direction()
    bpy.ops.object.editmode_toggle()
    
create_logo(1, (0,   0, 0), 1, 'linq2db', (0, 2.4, 0.75), 1,   (11, 3.5, 3.1), 1.15,    (1000, 10, 500, 500, 3))
#create_logo(2, (0, -10, 0), 1, 'DB',      (0, 2.5, 0),    1.9, (10, 3.5, 0.6), 1.15, ( 600,  20, 400, 400, 5))
