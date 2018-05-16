// Write your JavaScript code.

// Set up date-picker. See docs at https://bootstrap-datepicker.readthedocs.io/en/latest/markup.html
$('.datepicker').datepicker({
    language: 'sv',
    calendarWeeks: true,
    todayHighlight: true
});

$('.date .input-group-addon').click(function () {
    $(this).prev().datepicker('show');
});

$('#impersonation-select').change(function () {
    $('#impersonation-returnurl').val(window.location);
    $("#impersonation-form").submit();
});

$(function () {
    $("body").on("click", "table.clickable-rows > tbody > tr > td", function () {
        var $table = $(this).parents("table.clickable-rows");
        var $parameterName = $table.data("click-parameter");
        var $parameter = $(this).parent("tr").data($parameterName);
        window.location.href = tolkBaseUrl + $table.data("click-controller") + "/" + $table.data("click-action") + "?" + $parameterName + "=" + $parameter;
    });
});
