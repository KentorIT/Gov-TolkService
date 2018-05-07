// Write your JavaScript code.
$('.datepicker').datepicker(({
    language: 'sv',
    calendarWeeks: true
}));

$('.date .input-group-addon').click(function () {
    $(this).prev().datepicker('show');
})