module PandorasBoxMarioShell

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/shell" Shell(x::Integer, y::Integer, texture::String="koopa", lights::Bool=false, color::String="Green", colorSpeed::Number=0.8, direction::Integer=0, dangerous::Bool=false, grabbable::Bool=true)

const textures = Dict{String, String}(
    "Koopa" => "koopa",
    "Beetle" => "beetle",
    "Spiny" => "spiny",
    "Bowser Jr." => "bowserjr"
)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

const placements = Ahorn.PlacementDict(
    "Shell (Pandora's Box)" => Ahorn.EntityPlacement(
        Shell
    )
)

Ahorn.editingOptions(entity::Shell) = Dict{String, Any}(
    "texture" => textures,
    "color" => colors,
    "direction" => Dict{String, Any}(
        "Left" => -1,
        "Still" => 0,
        "Right" => 1
    )
)

function getColor(color)
    if haskey(Ahorn.XNAColors.colors, color)
        return Ahorn.XNAColors.colors[color]

    else
        try
            return ((Ahorn.argb32ToRGBATuple(parse(Int, replace(color, "#" => ""), base=16))[1:3] ./ 255)..., 1.0)

        catch

        end
    end

    return (1.0, 1.0, 1.0, 1.0)
end

function getTextures(entity::Shell)
    texture = get(entity.data, "texture", "koopa")

    return "objects/pandorasBox/shells/$texture/shell_idle00.png", "objects/pandorasBox/shells/$texture/deco_idle00.png"
end

function Ahorn.selection(entity::Shell)
    x, y = Ahorn.position(entity)

    shellSprite, decoSprite = getTextures(entity)

    return Ahorn.coverRectangles([
        Ahorn.getSpriteRectangle(shellSprite, x, y),
        Ahorn.getSpriteRectangle(decoSprite, x, y),
    ])
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Shell, room::Maple.Room)
    shellSprite, decoSprite = getTextures(entity)

    colorRaw = entity.color
    color = getColor(colorRaw)

    Ahorn.drawSprite(ctx, shellSprite, 0, 0, tint=color)
    Ahorn.drawSprite(ctx, decoSprite, 0, 0)
end

end