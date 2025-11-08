module PandorasBoxLaserNoteBlock

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/laserNoteBlock" LaserNoteBlock(x::Integer, y::Integer, pitch::Int=69, volume::Number=1.0, sound::String="game_03_deskbell_again", direction::String="Horizontal", atPlayer::Bool=false)

const directions = String["Horizontal", "Vertical"]

const placements = Ahorn.PlacementDict(
    "Laser Note Block ($direction) (Pandora's Box)" => Ahorn.EntityPlacement(
        LaserNoteBlock,
        "point",
        Dict{String, Any}(
            "direction" => direction
        )
    ) for direction in directions
)

Ahorn.editingOptions(entity::LaserNoteBlock) = Dict{String, Any}(
    "direction" => directions
)

const sprites = Dict{String, String}(
    "Horizontal" => "objects/pandorasBox/laser/noteblock/noteblock_horizontal",
    "Vertical" => "objects/pandorasBox/laser/noteblock/noteblock_vertical"
)

function Ahorn.selection(entity::LaserNoteBlock)
    x, y = Ahorn.position(entity)
    direction = get(entity, "direction", "Horizontal")
    sprite = get(sprites, direction, sprites["Horizontal"])

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=0.5)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LaserNoteBlock, room::Maple.Room)
    direction = get(entity, "direction", "Horizontal")
    sprite = get(sprites, direction, sprites["Horizontal"])

    Ahorn.drawSprite(ctx, sprite, 0, 0, jx=0.5, jy=0.5)
end

end