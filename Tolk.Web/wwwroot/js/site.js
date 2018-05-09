// Write your JavaScript code.

// Set up date-picker. See docs at https://bootstrap-datepicker.readthedocs.io/en/latest/markup.html
$('.datepicker').datepicker(({
    language: 'sv',
    calendarWeeks: true,
    todayHighlight: true
}));

$('.date .input-group-addon').click(function () {
    $(this).prev().datepicker('show');
})