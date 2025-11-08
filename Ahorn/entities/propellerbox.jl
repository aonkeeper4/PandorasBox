module PandorasBoxPropellerBox

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/propellerBox" PropellerBox(x::Integer, y::Integer, texture::String="default", flashUseColor::String="3F437C", flashChargeColor::String="5A1C1C", maxCharges::Integer=3, rechargeOnGround::Bool=true, glideMode::String="AfterUse")

const placements = Ahorn.PlacementDict(
    "Propeller Box (Pandora's Box)" => Ahorn.EntityPlacement(
        PropellerBox
    )
)

function getSprite(entity::PropellerBox)
    texture = get(entity, "texture", "default")

    return "objects/pandorasBox/propellerBox/$texture/default_charges00"
end

Ahorn.editingOptions(entity::PropellerBox) = Dict{String, Any}(
    "glideMode" => Dict{String, String}(
        "Always" => "Always",
        "After Using Charge" => "AfterUse",
        "Never" => "Never"
    )
)

function Ahorn.selection(entity::PropellerBox)
    x, y = Ahorn.position(entity)
    sprite = getSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y, jx=0.5, jy=1.0)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PropellerBox, room::Maple.Room) = Ahorn.drawSprite(ctx, getSprite(entity), 0, 0, jx=0.5, jy=1.0)

end