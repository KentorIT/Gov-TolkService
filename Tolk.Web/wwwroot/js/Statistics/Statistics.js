$(document).ready(function () {
    setReportDateText();
});

$("body").on("change", "#ReportType", function() {
    setReportDateText();
});

function setReportDateText() {
    if ($("#ReportType option:selected").val() === "") {
        $("#ReportDateHolder").html("Typ av datum som gäller för vald rapport");
    }
    else {
        $("#ReportDateHolder").html("För denna rapport kan du göra urval med " + $("#ReportType option:selected").attr('data-additional'));
    }
}