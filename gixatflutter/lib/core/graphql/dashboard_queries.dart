const String dashboardStatsQuery = r'''
  query DashboardStats {
    dashboardStats {
      todaySessions
      activeJobCards
      pendingAppointments
      carsInGarage
    }
  }
''';

const String todayAppointmentsQuery = r'''
  query TodayAppointments {
    todayAppointments {
      id
      time
      client {
        id
        name
      }
      vehicle {
        id
        model
        plateNumber
      }
      status
    }
  }
''';

const String activeJobCardsQuery = r'''
  query ActiveJobCards {
    activeJobCards {
      id
      jobNumber
      client {
        id
        name
      }
      vehicle {
        id
        model
        plateNumber
      }
      status
      assignedMechanic {
        id
        name
      }
    }
  }
''';

const String alertsQuery = r'''
  query Alerts {
    alerts {
      type
      message
      severity
      actionRequired
    }
  }
''';
