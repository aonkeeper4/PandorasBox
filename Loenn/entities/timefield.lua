local timefield = {}

timefield.name = "pandorasBox/timefield"
timefield.fillColor = {0.5, 1.0, 1.0, 0.4}
timefield.borderColor = {0.5, 1.0, 1.0, 1.0}
timefield.depth = 0
timefield.fieldInformation = {
    color = {
        fieldType = "color",
        allowXNAColors = true,
    }
}
timefield.placements = {
    name = "time_field",
    data = {
        width = 8,
        height = 8,
        start = 0.2,
        stop = 1.0,
        stopTime = 1.0,
        startTime = 3.0,
        animRate = 6.0,
        render = true,
        lingering = false,
        color = "Teal",
        respectOtherTimeRates = false
    }
}

return timefield