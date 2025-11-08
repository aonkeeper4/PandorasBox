module PandorasBoxTimefield

using ..Ahorn, Maple

@mapdef Entity "pandorasBox/timefield" Timefield(x::Integer, y::Integer, start::Number=0.2, stop::Number=1.0, stopTime::Number=1.0, startTime::Number=3.0, animRate::Number=6.0, render::Bool=true, lingering::Bool=false, color::String="Teal", respectOtherTimeRates::Bool=false)

const placements = Ahorn.PlacementDict(
    "Timefield (Pandora's Box)" => Ahorn.EntityPlacement(
        Timefield,
        "rectangle"
    ),
)

const colors = sort(collect(keys(Ahorn.XNAColors.colors)))

Ahorn.editingOptions(entity::Timefield) = Dict{String, Any}(
    "color" => colors
)

Ahorn.minimumSize(entity::Timefield) = 8, 8
Ahorn.resizable(entity::Timefield) = true, true

Ahorn.selection(entity::Timefield) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Timefield, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.5, 1.0, 1.0, 0.4), (0.5, 1.0, 1.0, 1.0))
end

end