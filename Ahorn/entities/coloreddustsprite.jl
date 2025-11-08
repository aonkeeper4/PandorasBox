module PandorasBoxDustSpriteColorController

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/dustSpriteColorController" DustSpriteColorController(x::Integer, y::Integer, eyeTexture::String="danger/dustcreature/eyes", eyeColor::String="Red", borderColor::String="Red,Green,Blue")

const placements = Ahorn.PlacementDict(
    "Dust Sprite Color Controller (Pandora's Box)" => Ahorn.EntityPlacement(
        DustSpriteColorController
    )
)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.editingOptions(entity::DustSpriteColorController) = Dict{String, Any}(
    "eyeColor" => colors
)

function Ahorn.selection(entity::DustSpriteColorController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 10, y - 10, 20, 20)
end

tintColor = (137, 251, 255, 255) ./ 255

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DustSpriteColorController, room::Maple.Room)
    Ahorn.Cairo.save(ctx)

    Ahorn.set_antialias(ctx, 1)
    Ahorn.set_line_width(ctx, 1);

    Ahorn.drawCircle(ctx, 0, 0, 10, (1.0, 1.0, 1.0, 1.0))

    Ahorn.Cairo.restore(ctx)

    Ahorn.drawSprite(ctx, "danger/dustcreature/base00", 0, 0, tint=tintColor)
end

end