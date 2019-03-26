
$(document).ready(function () {
    setReportDateText();
});

$("body").on("change", "#ReportType", function() {
    setReportDateText();
});

function setReportDateText() {
    if ($('#ReportType').val() === "Orders") {
        $("#ReportDateHolder").html("För denna rapport kan du göra urval med beställningsdatum");
    }
    else if ($('#ReportType').val() === "") {
        $("#ReportDateHolder").html("Typ av datum som gäller för vald rapport");
    }
    else {
        $("#ReportDateHolder").html("För denna rapport kan du göra urval med uppdragsdatum");
    }
}